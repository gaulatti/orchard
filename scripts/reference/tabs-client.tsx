import React from 'react';
import { createRoot } from 'react-dom/client';
import { Tabs } from 'frozen/tabs';

const root = createRoot(document.getElementById('root')!);
window.showTabs = (spec) => {
  window.tabRequests = [];
  const View = () => {
    const [active, setActive] = React.useState(spec.activeTab);
    return <Tabs {...spec} activeTab={active} aria-label="Component tabs"
      onChange={(id) => { window.tabRequests.push(id); if (spec.accept) setActive(id); }}
      renderLink={spec.customLink ? (props) => <a data-custom="true" href={props.item.href} className={props.className}>{props.children}</a> : undefined} />;
  };
  root.render(<View key={spec.id} />);
};
